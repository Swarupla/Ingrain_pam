import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdDeployedModelListComponent } from './ad-deployed-model-list.component';

describe('AdDeployedModelListComponent', () => {
  let component: AdDeployedModelListComponent;
  let fixture: ComponentFixture<AdDeployedModelListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AdDeployedModelListComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AdDeployedModelListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
