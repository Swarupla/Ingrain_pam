import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ModelEngineeringComponent } from './model-engineering.component';

describe('ModelEngineeringComponent', () => {
  let component: ModelEngineeringComponent;
  let fixture: ComponentFixture<ModelEngineeringComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ModelEngineeringComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ModelEngineeringComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
