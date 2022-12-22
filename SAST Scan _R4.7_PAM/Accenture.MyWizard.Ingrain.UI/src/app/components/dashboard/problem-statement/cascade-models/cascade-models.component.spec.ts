import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CascadeModelsComponent } from './cascade-models.component';

describe('CascadeModelsComponent', () => {
  let component: CascadeModelsComponent;
  let fixture: ComponentFixture<CascadeModelsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CascadeModelsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CascadeModelsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
