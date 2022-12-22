import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdUsecaseDefinitionComponent } from './ad-usecase-definition.component';

describe('AdUsecaseDefinitionComponent', () => {
  let component: AdUsecaseDefinitionComponent;
  let fixture: ComponentFixture<AdUsecaseDefinitionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AdUsecaseDefinitionComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AdUsecaseDefinitionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
